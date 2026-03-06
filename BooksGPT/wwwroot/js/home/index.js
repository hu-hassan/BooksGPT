// Home Index JavaScript
(function () {
    'use strict';

    // Utility Functions
    function renderGemini(text) {
        if (!text) return '';
        return marked.parse(text);
    }

    function escapeHtml(text) {
        return (text || '').replace(/[&<>"']/g, function (m) {
            return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[m];
        });
    }

    function getCookie(name) {
        const v = document.cookie.match('(^|;)\\s*' + name + '\\s*=\\s*([^;]+)');
        return v ? decodeURIComponent(v.pop()) : '';
    }

    function setCookie(name, value, days) {
        var d = new Date();
        d.setTime(d.getTime() + (days || 365) * 24 * 60 * 60 * 1000);
        document.cookie = name + '=' + encodeURIComponent(value) + ';path=/;expires=' + d.toUTCString();
    }

    // Track current chat ID - initialize from server if available
    var currentChatId = window.currentChatIdFromServer || '';

    // Helper function to set active chat in sidebar
    function setActiveChat(chatId) {
        // Remove active class from all history items
        $('.history-item').removeClass('active');
        
        // Add active class to the selected chat
        if (chatId) {
            $('.history-item[data-id="' + chatId + '"]').addClass('active');
        }
    }

    // Render conversation on page reload if data exists
    function renderReloadData() {
        if (window.chatReloadData) {
            var userQuestions = window.chatReloadData.userQuestions || [];
            var botAnswers = window.chatReloadData.botAnswers || [];
            var reloadChatId = window.chatReloadData.currentChatId || '';
            var html = '';

            for (var i = 0; i < botAnswers.length; i++) {
                html += '<div class="message bot-message">' + renderGemini(botAnswers[i]) + '</div>';
                if (i < userQuestions.length) {
                    html += '<div class="message user-message">' + escapeHtml(userQuestions[i]) + '</div>';
                }
            }

            $('#chat-box').html(html);

            // Scroll to bottom
            var chatBox = document.getElementById('chat-box');
            if (chatBox) chatBox.scrollTop = chatBox.scrollHeight;

            // Highlight the current chat in sidebar on reload
            if (reloadChatId) {
                currentChatId = reloadChatId;
                setActiveChat(reloadChatId);
            }

            // Clean up
            delete window.chatReloadData;
        }
    }

    // Load chat history by ID
    function loadChatHistory(chatId) {
        // Hide any open history menus immediately
        $('.history-menu').hide();
        
        $.get('/Home/GetChatHistoryById', { id: chatId }, function (data) {
            if (!data || !data.success) {
                // If redirect flag is set, chat is incomplete - start new chat
                if (data && data.redirect) {
                    location.reload();
                }
                return;
            }
            currentChatId = String(chatId);
            
            // Highlight the selected chat in sidebar
            setActiveChat(chatId);
            
            const userQuestions = data.userQuestions || [];
            const botAnswers = data.botAnswers || [];
            let html = '';
            for (let i = 0; i < botAnswers.length; i++) {
                html += '<div class="message bot-message">' + renderGemini(botAnswers[i]) + '</div>';
                if (i < userQuestions.length) {
                    html += '<div class="message user-message">' + escapeHtml(userQuestions[i]) + '</div>';
                }
            }
            $('#chat-box').html(html);
        });
    }

    // Send message to server
    function sendMessage() {
        const userInput = $('#user-input').val().trim();
        if (!userInput) return;

        // Add user message to chat
        $('#chat-box').append('<div class="message user-message" id="latest-user-message">' + escapeHtml(userInput) + '</div>');
        $('#user-input').val('');

        // Scroll to show user message
        var $chatBox = $('#chat-box');
        var $latestUserMsg = $('#latest-user-message');
        if ($latestUserMsg.length) {
            var msgBottom = $latestUserMsg.position().top + $latestUserMsg.outerHeight() + $chatBox.scrollTop();
            $chatBox.scrollTop(msgBottom - $chatBox.height());
            $latestUserMsg.removeAttr('id');
        }

        // Show typing indicator
        const typingHtml = '<div class="message bot-message typing-indicator" id="typing-indicator">' +
            '<span class="typing-dot"></span><span class="typing-dot"></span><span class="typing-dot"></span></div>';
        $chatBox.append(typingHtml);

        var $typingIndicator = $('#typing-indicator');
        if ($typingIndicator.length) {
            var typingBottom = $typingIndicator.position().top + $typingIndicator.outerHeight() + $chatBox.scrollTop();
            if (typingBottom > $chatBox.scrollTop() + $chatBox.height()) {
                $chatBox.scrollTop(typingBottom - $chatBox.height());
            }
        }

        // Send AJAX request
        $.ajax({
            url: 'Home/Post',
            type: 'POST',
            data: { userInput: userInput },
            success: function (data) {
                $('#typing-indicator').remove();
                $chatBox.append('<div class="message bot-message">' + renderGemini(data.reply) + '</div>');

                var $latestUserMsg = $chatBox.find('.user-message').last();
                if ($latestUserMsg.length) {
                    var msgBottom = $latestUserMsg.position().top + $latestUserMsg.outerHeight() + $chatBox.scrollTop();
                    $chatBox.scrollTop(msgBottom - $chatBox.height());
                }

                // If new chat was created, add to sidebar
                if (data.newChat && data.newChat.id && data.newChat.title) {
                    currentChatId = String(data.newChat.id);
                    
                    var newChatHtml = '<div class="history-item active" data-id="' + data.newChat.id + '">' +
                        '<div class="history-title" style="flex:1;cursor:pointer;">' + escapeHtml(data.newChat.title) + '</div>' +
                        '<div style="margin-left:8px;position:relative;">' +
                        '<button class="history-menu-btn" style="background:transparent;border:none;color:#9ca3af;cursor:pointer;padding:4px 6px;">&#x22EF;</button>' +
                        '<div class="history-menu" style="display:none;position:absolute;right:0;top:100%;background:#0b0b0b;padding:6px;border-radius:8px;box-shadow:0 8px 20px rgba(0,0,0,0.6);z-index:1200;">' +
                        '<button class="delete-chat" style="background:transparent;border:none;color:#fff;cursor:pointer;padding:6px 8px;">Delete</button>' +
                        '</div></div></div>';
                    
                    // Remove active from other chats first
                    $('.history-item').removeClass('active');
                    
                    $('.history-list').prepend(newChatHtml);
                    
                    // Also add to mobile sidebar
                    $('.mobile-sidebar-list').prepend(newChatHtml);

                    // Remove "No chat history" message if it exists
                    $('.history-list').find('.history-item:contains("No chat history")').remove();
                    $('.mobile-sidebar-list').find('.history-item:contains("No chat history")').remove();
                }
            },
            error: function () {
                $('#typing-indicator').remove();
                $chatBox.append('<div class="message bot-message">Sorry, there was an error processing your request.</div>');
            }
        });
    }

    // Start new chat
    function startNewChat() {
        $.post('/Home/NewChat', function (resp) {
            if (resp && resp.success) {
                $('#chat-box').html('<div class="message bot-message">' + escapeHtml(resp.aiReply || 'Enter the book title to search') + '</div>');
                $('#user-input').val('');
                currentChatId = '';
                setCookie('CurrentChatId', '');
                $('.history-menu').hide();
                
                // Remove active class from all chats (new chat has no ID yet)
                setActiveChat(null);
            } else {
                alert((resp && resp.error) || 'Failed to start new chat');
            }
        }).fail(function () {
            alert('Error starting new chat');
        });
    }
    
    // Expose startNewChat globally for mobile sidebar
    window.startNewChat = startNewChat;
    window.loadChatHistory = loadChatHistory;

    // Initialize on document ready
    $(function () {
        // Render reload data if exists
        renderReloadData();

        // History item click - load chat (desktop sidebar only, not mobile)
        $(document).on('click', '.history-list .history-item', function (e) {
            if ($(e.target).closest('.history-menu, .history-menu-btn, .delete-chat').length) return;
            var id = $(this).data('id');
            console.debug('history-item clicked, id=', id);
            // Hide any open history menus immediately when clicking a chat
            $('.history-menu').hide();
            if (id) loadChatHistory(id);
        });

        // New chat button
        $('#new-chat-btn').on('click', startNewChat);

        // Profile menu toggle
        $('#left-user-toggle').on('click', function (e) {
            e.preventDefault();
            $('#left-user-menu').toggle();
        });

        // Logout
        $('#left-logout-link').on('click', function (e) {
            e.preventDefault();
            $.post('/Login/Logout', function () {
                window.location.href = '/login';
            });
        });

        // Edit profile
        $('#left-edit-profile').on('click', function (e) {
            e.preventDefault();
            $.get('/Home/GetProfile', function (resp) {
                if (!resp || !resp.success) {
                    alert(resp?.error || 'Failed');
                    return;
                }
                $('#profile-name').val(resp.name || '');
                $('#profile-username').val(resp.username || '');
                try {
                    $('#profile-color').val(resp.avatarColor || '#10a37f');
                } catch {
                    $('#profile-color').val('#10a37f');
                }
                $('#edit-profile-modal').show();
                $('#left-user-menu').hide();
            });
        });

        // Modal close handlers
        $('#edit-profile-modal').on('click', function (e) {
            if (e.target.id === 'edit-profile-modal') {
                $('#edit-profile-modal').hide();
            }
        });

        $('#modal-close, #modal-cancel').on('click', function () {
            $('#edit-profile-modal').hide();
        });

        // Save profile
        $('#modal-save').on('click', function () {
            var newName = $('#profile-name').val().trim();
            var newUsername = $('#profile-username').val().trim();
            var newColor = $('#profile-color').val();

            $.post('/Home/UpdateProfile', {
                name: newName,
                username: newUsername,
                avatarColor: newColor
            }, function (resp) {
                if (!resp || !resp.success) {
                    alert(resp?.error || 'Failed');
                    return;
                }
                setCookie('name', newName);
                setCookie('avatarColor', newColor);
                $('#left-user-name').text(newName || 'Guest');
                $('#left-avatar').css('background', newColor || '#6b7280');
                // Also update mobile sidebar
                $('#mobile-user-name').text(newName || 'Guest');
                $('#mobile-avatar').css('background', newColor || '#6b7280');
                $('#edit-profile-modal').hide();
            }).fail(function () {
                alert('Error saving profile');
            });
        });

        // Close history menus when clicking outside
        $(document).on('click', function (e) {
            if (!$(e.target).closest('.history-menu, .history-menu-btn').length) {
                $('.history-menu').hide();
            }
        });

        // History menu toggle (desktop only — mobile has its own handler below)
        $(document).on('click', '.history-menu-btn', function (e) {
            // Skip if inside mobile sidebar — handled separately with fixed positioning
            if ($(this).closest('.mobile-sidebar-list').length) return;
            e.stopPropagation();
            var $menu = $(this).siblings('.history-menu');
            $('.history-menu').not($menu).hide();
            $menu.toggle();
        });

        // Delete chat
        $(document).on('click', '.delete-chat', function (e) {
            e.stopPropagation();
            var $historyItem = $(this).closest('.history-item');
            var id = $historyItem.data('id');

            if (!confirm('Delete this chat?')) return;

            $.post('/Home/DeleteChat', { id: id }, function (resp) {
                if (resp && resp.success) {
                    // Remove from both sidebars
                    $('.history-item[data-id="' + id + '"]').remove();
                    if (String(currentChatId) === String(id)) {
                        // Current chat was deleted - start new chat
                        startNewChat();
                    }
                } else {
                    alert(resp?.error || 'Failed to delete chat');
                }
            }).fail(function () {
                alert('Error deleting chat');
            });
        });

        // Send button click
        $('#send-button').on('click', sendMessage);

        // Enter key to send (without shift)
        $('#user-input').on('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });

        // =====================
        // Mobile Sidebar Logic
        // =====================

        function checkMobile() {
            if (window.innerWidth <= 768) {
                $('#mobile-sidebar-btn').show();
            } else {
                $('#mobile-sidebar-btn').hide();
                closeMobileSidebar();
            }
        }

        function openMobileSidebar() {
            $('#mobile-sidebar').addClass('open');
            $('#mobile-sidebar-overlay').addClass('open');
            $('body').css('overflow', 'hidden');
        }

        function closeMobileSidebar() {
            $('#mobile-sidebar').removeClass('open');
            $('#mobile-sidebar-overlay').removeClass('open');
            $('body').css('overflow', '');
            $('#mobile-user-menu').hide();
            $('.mobile-sidebar-list .history-menu').hide();
        }

        // Check mobile on load and resize
        checkMobile();
        $(window).on('resize', checkMobile);

        // Open mobile sidebar
        $(document).on('click', '#mobile-sidebar-btn', function(e) {
            e.stopPropagation();
            openMobileSidebar();
        });

        // Close mobile sidebar
        $(document).on('click', '#mobile-sidebar-close, #mobile-sidebar-overlay', function() {
            closeMobileSidebar();
        });

        // Selecting a chat closes sidebar and loads via AJAX (clicking anywhere on the card)
        $(document).on('click', '.mobile-sidebar-list .history-item', function(e) {
            e.stopPropagation();
            // Ignore clicks on menu button, delete button, or the open menu itself
            if ($(e.target).closest('.history-menu-btn, .history-menu, .delete-chat').length) return;
            var chatId = $(this).data('id');
            // Hide menus before closing sidebar
            $('.mobile-sidebar-list .history-menu').hide();
            closeMobileSidebar();
            if (chatId) {
                loadChatHistory(chatId);
            }
        });

        // Menu toggle for mobile sidebar items — same as desktop, just toggle
        $(document).on('click', '.mobile-sidebar-list .history-menu-btn', function(e) {
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
            var $menu = $(this).siblings('.history-menu');
            // Hide all other mobile menus
            $('.mobile-sidebar-list .history-menu').not($menu).hide();
            $menu.toggle();
        });

        // New chat from mobile sidebar
        $(document).on('click', '#mobile-new-chat-btn', function() {
            closeMobileSidebar();
            startNewChat();
        });

        // Mobile user menu toggle
        $(document).on('click', '#mobile-user-toggle', function(e) {
            e.stopPropagation();
            $('#mobile-user-menu').toggle();
        });

        // Mobile edit profile
        $(document).on('click', '.mobile-edit-profile', function(e) {
            e.preventDefault();
            closeMobileSidebar();
            $.get('/Home/GetProfile', function(resp) {
                if (!resp || !resp.success) {
                    alert(resp?.error || 'Failed');
                    return;
                }
                $('#profile-name').val(resp.name || '');
                $('#profile-username').val(resp.username || '');
                try {
                    $('#profile-color').val(resp.avatarColor || '#10a37f');
                } catch(err) {
                    $('#profile-color').val('#10a37f');
                }
                $('#edit-profile-modal').show();
            });
        });

        // Mobile logout
        $(document).on('click', '.mobile-logout-link', function(e) {
            e.preventDefault();
            closeMobileSidebar();
            $.post('/Login/Logout', function() {
                window.location.href = '/login';
            });
        });

        // Close menus when clicking elsewhere in sidebar
        $(document).on('click', '#mobile-sidebar', function(e) {
            if (!$(e.target).closest('.history-item, .history-menu-btn, .history-menu, #mobile-user-toggle, #mobile-user-menu').length) {
                $('.mobile-sidebar-list .history-menu').hide();
                $('#mobile-user-menu').hide();
            }
        });
    });
})();
